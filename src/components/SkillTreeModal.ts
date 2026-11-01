import { MonoBehaviour } from '../core/MonoBehaviour.ts';
import { GameState } from '../core/GameState.ts';
import { EventBus } from '../core/EventBus.ts';
import { SKILL_TREE_NODES, SkillNode } from '../data/SkillTreeData.ts';

/**
 * SkillTreeModal implements the Star Wars Jedi: Fallen Order radial constellation skill tree:
 * - Rendered in a centered ~60% box window floating over the detective desk
 * - Inspection Details Box positioned underneath the upgrades in the bottom-middle
 * - 3 Branch Sectors: Cartography (Left), Chronometry (Center), Criminology (Right)
 * - Concentric orbital guide arcs and connecting constellation vector lines
 * - Progressive Fog-of-War reveals: only unblocked nodes and adjacent mysteries are revealed
 * - Bottom currency and constellation completion tracker
 * Maps directly to Unity's RadialSkillTreeUI / ConstellationModal.
 */
export class SkillTreeModal extends MonoBehaviour {
  public isOpen: boolean = false;
  private selectedNodeId: string = 'globe_root';

  // DOM element caches
  private modalContainer!: HTMLElement;
  private svgConstellation!: SVGSVGElement;
  private detailsPanel!: HTMLElement;
  private btnClose!: HTMLElement;
  private btnOpenHUD!: HTMLElement;
  private bottomFundsEl!: HTMLElement;
  private bottomGoldEl!: HTMLElement;
  private bottomProgressEl!: HTMLElement;

  constructor(
    private gameState: GameState,
    private eventBus: EventBus
  ) {
    super();
  }

  public override awake(): void {
    this.modalContainer = document.getElementById('skill-tree-modal')!;
    this.svgConstellation = document.getElementById('constellation-svg') as unknown as SVGSVGElement;
    this.detailsPanel = document.getElementById('skill-details-panel')!;
    this.btnClose = document.getElementById('btn-close-skills')!;
    this.btnOpenHUD = document.getElementById('btn-open-skills')!;
    this.bottomFundsEl = document.getElementById('tree-val-funds')!;
    this.bottomGoldEl = document.getElementById('tree-val-gold')!;
    this.bottomProgressEl = document.getElementById('tree-val-progress')!;

    if (this.btnOpenHUD) {
      this.btnOpenHUD.onclick = () => this.open();
    }

    if (this.btnClose) {
      this.btnClose.onclick = () => this.close();
    }

    // Keyboard support: Escape closes the modal
    window.addEventListener('keydown', (e) => {
      if (e.key === 'Escape' && this.isOpen) {
        this.close();
      }
    });

    // Close when clicking outside the 60% box window
    if (this.modalContainer) {
      this.modalContainer.addEventListener('mousedown', (e) => {
        if (e.target === this.modalContainer || (e.target as HTMLElement).classList.contains('modal-backdrop')) {
          this.close();
        }
      });
    }

    this.renderConstellation();
    this.updateDetailsPanel();
  }

  public open(): void {
    this.isOpen = true;
    if (this.modalContainer) {
      this.modalContainer.classList.add('open');
    }
    this.eventBus.emit('audioPlay', 'click');
    this.renderConstellation();
    this.updateDetailsPanel();
    this.updateBottomBar();
  }

  public close(): void {
    this.isOpen = false;
    if (this.modalContainer) {
      this.modalContainer.classList.remove('open');
    }
    this.eventBus.emit('audioPlay', 'click');
  }

  public override update(_deltaTime: number): void {
    if (!this.isOpen) return;
    this.updateBottomBar();
    this.updateAcquireButtonState();
  }

  private updateBottomBar(): void {
    if (this.bottomFundsEl) {
      this.bottomFundsEl.textContent = `€${this.gameState.funds.toFixed(2)}`;
    }
    if (this.bottomGoldEl) {
      this.bottomGoldEl.textContent = `🪙 ${this.gameState.goldCoins}`;
    }
    if (this.bottomProgressEl) {
      const total = SKILL_TREE_NODES.length;
      const count = this.gameState.purchasedSkills.size;
      this.bottomProgressEl.textContent = `${count} / ${total} BLUEPRINTS`;
    }
  }

  /**
   * Evaluates the node visibility status:
   * - 'purchased': Node has been acquired
   * - 'available': Prerequisite is purchased (or root node) and ready to be unlocked
   * - 'mystery': 1 step beyond available (fog-of-war outline visible, details classified)
   * - 'hidden': 2+ steps beyond available (completely concealed in darkness)
   */
  private getNodeVisibility(node: SkillNode): 'purchased' | 'available' | 'mystery' | 'hidden' {
    if (this.gameState.isSkillPurchased(node.id)) {
      return 'purchased';
    }

    // If root node (no prereq)
    if (!node.prereqId) {
      return 'available';
    }

    // If direct parent is purchased, this node is AVAILABLE
    if (this.gameState.isSkillPurchased(node.prereqId)) {
      return 'available';
    }

    // Check if grandparent is purchased (1 step ahead = mystery)
    const parentNode = SKILL_TREE_NODES.find((n) => n.id === node.prereqId);
    if (parentNode) {
      if (!parentNode.prereqId || this.gameState.isSkillPurchased(parentNode.prereqId)) {
        return 'mystery';
      }
    }

    return 'hidden';
  }

  /**
   * Renders SVG arcs, connector lines, and circular constellation nodes.
   */
  public renderConstellation(): void {
    if (!this.svgConstellation) return;

    this.svgConstellation.innerHTML = '';
    const NS = 'http://www.w3.org/2000/svg';

    // 1. Defs & Glow filters
    const defs = document.createElementNS(NS, 'defs');
    defs.innerHTML = `
      <radialGradient id="nodePurchasedGrad" cx="50%" cy="50%" r="50%">
        <stop offset="0%" stop-color="#2a3f5f" />
        <stop offset="80%" stop-color="#141c2b" />
        <stop offset="100%" stop-color="#0e1420" />
      </radialGradient>
      <radialGradient id="nodeAvailableGrad" cx="50%" cy="50%" r="50%">
        <stop offset="0%" stop-color="#4a3720" />
        <stop offset="80%" stop-color="#241a10" />
        <stop offset="100%" stop-color="#150f08" />
      </radialGradient>
      <radialGradient id="nodeGoldGrad" cx="50%" cy="50%" r="50%">
        <stop offset="0%" stop-color="#856417" />
        <stop offset="80%" stop-color="#3d2d08" />
        <stop offset="100%" stop-color="#1c1403" />
      </radialGradient>
      <filter id="glowGold" x="-30%" y="-30%" width="160%" height="160%">
        <feGaussianBlur stdDeviation="3.5" result="blur" />
        <feMerge>
          <feMergeNode in="blur" />
          <feMergeNode in="SourceGraphic" />
        </feMerge>
      </filter>
      <filter id="glowCyan" x="-30%" y="-30%" width="160%" height="160%">
        <feGaussianBlur stdDeviation="3.5" result="blur" />
        <feMerge>
          <feMergeNode in="blur" />
          <feMergeNode in="SourceGraphic" />
        </feMerge>
      </filter>
    `;
    this.svgConstellation.appendChild(defs);

    // 2. Background Concentric Orbital Guide Arcs
    const arcGroup = document.createElementNS(NS, 'g');
    arcGroup.setAttribute('class', 'constellation-arcs');

    const arcCenterY = 540;
    const radii = [120, 205, 290, 375, 455];
    for (const r of radii) {
      const circle = document.createElementNS(NS, 'circle');
      circle.setAttribute('cx', '500');
      circle.setAttribute('cy', `${arcCenterY}`);
      circle.setAttribute('r', `${r}`);
      circle.setAttribute('fill', 'none');
      circle.setAttribute('stroke', 'rgba(90, 115, 145, 0.12)');
      circle.setAttribute('stroke-width', '1');
      circle.setAttribute('stroke-dasharray', '4 6');
      arcGroup.appendChild(circle);
    }

    // Radial guide spokes
    const spokeAngles = [-60, -35, 0, 35, 60];
    for (const deg of spokeAngles) {
      const rad = ((deg - 90) * Math.PI) / 180;
      const x2 = 500 + Math.cos(rad) * 480;
      const y2 = arcCenterY + Math.sin(rad) * 480;
      const spoke = document.createElementNS(NS, 'line');
      spoke.setAttribute('x1', '500');
      spoke.setAttribute('y1', `${arcCenterY}`);
      spoke.setAttribute('x2', `${x2}`);
      spoke.setAttribute('y2', `${y2}`);
      spoke.setAttribute('stroke', 'rgba(90, 115, 145, 0.08)');
      spoke.setAttribute('stroke-width', '1');
      arcGroup.appendChild(spoke);
    }

    this.svgConstellation.appendChild(arcGroup);

    // 3. Sector Header Labels
    const labelGroup = document.createElementNS(NS, 'g');
    labelGroup.setAttribute('class', 'sector-labels');
    labelGroup.innerHTML = `
      <!-- Left: Cartography -->
      <text x="210" y="115" text-anchor="middle" class="sector-title">CARTOGRAPHY</text>
      <text x="210" y="130" text-anchor="middle" class="sector-sub">PINS &amp; MATRIX</text>

      <!-- Center: Chronometry -->
      <text x="500" y="55" text-anchor="middle" class="sector-title">CHRONOMETRY</text>
      <text x="500" y="70" text-anchor="middle" class="sector-sub">MECHANICAL REVOLUTIONS</text>

      <!-- Right: Criminology -->
      <text x="790" y="115" text-anchor="middle" class="sector-title">CRIMINOLOGY</text>
      <text x="790" y="130" text-anchor="middle" class="sector-sub">CASE DOSSIERS &amp; BOUNTIES</text>
    `;
    this.svgConstellation.appendChild(labelGroup);

    // 4. Constellation Connector Lines (Parent -> Child)
    const lineGroup = document.createElementNS(NS, 'g');
    lineGroup.setAttribute('class', 'constellation-lines');

    for (const node of SKILL_TREE_NODES) {
      if (!node.prereqId) continue;
      const parent = SKILL_TREE_NODES.find((n) => n.id === node.prereqId);
      if (!parent) continue;

      const nodeVis = this.getNodeVisibility(node);
      const parentVis = this.getNodeVisibility(parent);

      if (nodeVis === 'hidden' && parentVis === 'hidden') continue;

      const line = document.createElementNS(NS, 'line');
      line.setAttribute('x1', `${parent.x}`);
      line.setAttribute('y1', `${parent.y}`);
      line.setAttribute('x2', `${node.x}`);
      line.setAttribute('y2', `${node.y}`);

      if (nodeVis === 'purchased') {
        line.setAttribute('stroke', node.currency === 'gold' ? '#f59e0b' : '#38bdf8');
        line.setAttribute('stroke-width', '2.5');
        line.setAttribute('filter', node.currency === 'gold' ? 'url(#glowGold)' : 'url(#glowCyan)');
      } else if (nodeVis === 'available') {
        line.setAttribute('stroke', 'rgba(148, 163, 184, 0.45)');
        line.setAttribute('stroke-width', '1.8');
      } else if (nodeVis === 'mystery') {
        line.setAttribute('stroke', 'rgba(100, 116, 139, 0.22)');
        line.setAttribute('stroke-width', '1.4');
        line.setAttribute('stroke-dasharray', '3 4');
      } else {
        continue;
      }

      lineGroup.appendChild(line);
    }

    this.svgConstellation.appendChild(lineGroup);

    // 5. Constellation Nodes (Interactive circular buttons)
    const nodeGroup = document.createElementNS(NS, 'g');
    nodeGroup.setAttribute('class', 'constellation-nodes');

    for (const node of SKILL_TREE_NODES) {
      const vis = this.getNodeVisibility(node);
      if (vis === 'hidden') continue;

      const isSelected = this.selectedNodeId === node.id;
      const g = document.createElementNS(NS, 'g');
      g.setAttribute('class', `node-g node-${vis} ${isSelected ? 'selected' : ''}`);
      g.setAttribute('transform', `translate(${node.x}, ${node.y})`);
      g.setAttribute('cursor', 'pointer');

      // Click to select node
      g.onclick = () => {
        this.selectedNodeId = node.id;
        this.eventBus.emit('audioPlay', 'click');
        this.renderConstellation();
        this.updateDetailsPanel();
      };

      // Node selection target ring
      if (isSelected) {
        const selRing = document.createElementNS(NS, 'circle');
        selRing.setAttribute('r', '27');
        selRing.setAttribute('fill', 'none');
        selRing.setAttribute('stroke', node.currency === 'gold' ? '#ffd700' : '#38bdf8');
        selRing.setAttribute('stroke-width', '2');
        selRing.setAttribute('stroke-dasharray', '5 3');
        selRing.setAttribute('class', 'selection-spinner');
        g.appendChild(selRing);
      }

      // Outer halo ring
      const outerRing = document.createElementNS(NS, 'circle');
      outerRing.setAttribute('r', '22');
      outerRing.setAttribute('fill', 'none');

      // Inner background circle
      const innerCircle = document.createElementNS(NS, 'circle');
      innerCircle.setAttribute('r', '20');

      if (vis === 'purchased') {
        outerRing.setAttribute('stroke', node.currency === 'gold' ? '#ffd700' : '#ffffff');
        outerRing.setAttribute('stroke-width', '2.5');
        outerRing.setAttribute('filter', node.currency === 'gold' ? 'url(#glowGold)' : 'url(#glowCyan)');
        innerCircle.setAttribute('fill', node.currency === 'gold' ? 'url(#nodeGoldGrad)' : 'url(#nodePurchasedGrad)');
      } else if (vis === 'available') {
        const canAfford =
          node.currency === 'gold' ? this.gameState.goldCoins >= node.cost : this.gameState.funds >= node.cost;

        outerRing.setAttribute('stroke', canAfford ? '#fbbf24' : 'rgba(217, 119, 6, 0.45)');
        outerRing.setAttribute('stroke-width', '2');
        outerRing.setAttribute('class', 'pulse-ring');
        innerCircle.setAttribute('fill', 'url(#nodeAvailableGrad)');
      } else {
        // Mystery node
        outerRing.setAttribute('stroke', 'rgba(75, 85, 99, 0.4)');
        outerRing.setAttribute('stroke-width', '1.5');
        outerRing.setAttribute('stroke-dasharray', '3 3');
        innerCircle.setAttribute('fill', '#0d131f');
      }

      g.appendChild(outerRing);
      g.appendChild(innerCircle);

      // Icon / Glyph inside circle
      const textIcon = document.createElementNS(NS, 'text');
      textIcon.setAttribute('text-anchor', 'middle');
      textIcon.setAttribute('dominant-baseline', 'central');
      textIcon.setAttribute('font-size', '15');

      if (vis === 'mystery') {
        textIcon.textContent = '🔒';
        textIcon.setAttribute('opacity', '0.45');
      } else {
        textIcon.textContent = node.icon;
        textIcon.setAttribute('opacity', vis === 'purchased' ? '1.0' : '0.88');
      }
      g.appendChild(textIcon);

      // Mini currency badge for Gold items
      if (node.currency === 'gold' && vis !== 'mystery') {
        const goldPip = document.createElementNS(NS, 'circle');
        goldPip.setAttribute('cx', '13');
        goldPip.setAttribute('cy', '-13');
        goldPip.setAttribute('r', '4.5');
        goldPip.setAttribute('fill', '#ffd700');
        goldPip.setAttribute('stroke', '#000000');
        goldPip.setAttribute('stroke-width', '1');
        g.appendChild(goldPip);
      }

      nodeGroup.appendChild(g);
    }

    this.svgConstellation.appendChild(nodeGroup);
  }

  /**
   * Updates the Information Details Box positioned underneath the upgrades in the center (red box).
   */
  public updateDetailsPanel(): void {
    if (!this.detailsPanel) return;

    const node = SKILL_TREE_NODES.find((n) => n.id === this.selectedNodeId);
    if (!node) return;

    const vis = this.getNodeVisibility(node);

    if (vis === 'mystery') {
      this.detailsPanel.innerHTML = `
        <div class="panel-header-line">
          <span class="panel-branch">ARCHIVES CLASSIFIED</span>
          <span class="panel-cost" style="color: #64748b;">PREREQUISITE REQUIRED</span>
        </div>
        <div class="panel-title">[ LOCKED BLUEPRINT ]</div>
        <div class="panel-desc">
          This sector is encrypted in the department archives. 
          Acquire the preceding connected blueprint node to unlock this discovery!
        </div>
        <button class="panel-btn disabled" disabled>PREREQUISITE REQUIRED</button>
      `;
      return;
    }

    const isPurchased = vis === 'purchased';
    const costText = node.currency === 'gold' ? `🪙 ${node.cost} GOLD COINS` : `€${node.cost.toFixed(2)}`;
    const costColor = node.currency === 'gold' ? '#ffd700' : '#38bdf8';

    const canAfford =
      node.currency === 'gold' ? this.gameState.goldCoins >= node.cost : this.gameState.funds >= node.cost;

    let buttonHtml = '';
    if (isPurchased) {
      buttonHtml = `<button class="panel-btn acquired" disabled>✔ BLUEPRINT ACQUIRED</button>`;
    } else if (canAfford) {
      buttonHtml = `<button id="btn-acquire-skill" class="panel-btn active">ACQUIRE BLUEPRINT (${costText})</button>`;
    } else {
      buttonHtml = `<button class="panel-btn disabled" disabled>INSUFFICIENT FUNDS (${costText})</button>`;
    }

    this.detailsPanel.innerHTML = `
      <div class="panel-header-line">
        <span class="panel-branch">${node.branchTitle} // SECTOR ${node.tier}</span>
        <span class="panel-cost" style="color: ${costColor};">${costText}</span>
      </div>
      <div class="panel-title">${node.title}</div>
      <div class="panel-desc">${node.desc}</div>
      ${buttonHtml}
    `;

    // Bind acquire button
    const btnAcquire = document.getElementById('btn-acquire-skill');
    if (btnAcquire) {
      btnAcquire.onclick = () => {
        this.buySelectedSkill();
      };
    }
  }

  private updateAcquireButtonState(): void {
    const node = SKILL_TREE_NODES.find((n) => n.id === this.selectedNodeId);
    if (!node) return;

    const vis = this.getNodeVisibility(node);
    if (vis !== 'available') return;

    const canAfford =
      node.currency === 'gold' ? this.gameState.goldCoins >= node.cost : this.gameState.funds >= node.cost;

    const btn = document.getElementById('btn-acquire-skill') as HTMLButtonElement | null;
    if (btn) {
      if (canAfford) {
        btn.disabled = false;
        btn.classList.remove('disabled');
        btn.classList.add('active');
      } else {
        btn.disabled = true;
        btn.classList.remove('active');
        btn.classList.add('disabled');
      }
    }
  }

  private buySelectedSkill(): void {
    const node = SKILL_TREE_NODES.find((n) => n.id === this.selectedNodeId);
    if (!node) return;

    const vis = this.getNodeVisibility(node);
    if (vis !== 'available') return;

    // Deduct appropriate currency
    let success = false;
    if (node.currency === 'gold') {
      success = this.gameState.deductGoldCoins(node.cost);
    } else {
      success = this.gameState.deductFunds(node.cost);
    }

    if (!success) {
      this.eventBus.emit('audioPlay', 'error');
      return;
    }

    // Apply skill
    this.gameState.purchasedSkills.add(node.id);
    node.apply(this.gameState);

    this.eventBus.emit('audioPlay', 'win');
    this.eventBus.emit('floatingText', {
      x: window.innerWidth * 0.5,
      y: window.innerHeight * 0.35,
      text: `★ UNLOCKED: ${node.title}! ★`,
      color: node.currency === 'gold' ? '#ffd700' : '#38bdf8'
    });

    // Re-render constellation with newly unveiled branches
    this.renderConstellation();
    this.updateDetailsPanel();
    this.updateBottomBar();
  }
}
