namespace DetectiveDesk.Gameplay
{
    /// <summary>Major world cities used for geographic pin placement and labels.</summary>
    public static class WorldCities
    {
        public readonly struct City
        {
            public readonly string Name;
            public readonly float Latitude, Longitude;
            public City(string name, float lat, float lon) { Name = name; Latitude = lat; Longitude = lon; }
        }

        public static readonly City[] All =
        {
            new City("Tokyo", 35.68f, 139.69f), new City("Delhi", 28.61f, 77.21f), new City("Shanghai", 31.23f, 121.47f),
            new City("São Paulo", -23.55f, -46.63f), new City("Mexico City", 19.43f, -99.13f), new City("Cairo", 30.04f, 31.24f),
            new City("Mumbai", 19.08f, 72.88f), new City("Beijing", 39.90f, 116.41f), new City("Dhaka", 23.81f, 90.41f),
            new City("Osaka", 34.69f, 135.50f), new City("New York", 40.71f, -74.01f), new City("Karachi", 24.86f, 67.01f),
            new City("Buenos Aires", -34.60f, -58.38f), new City("Istanbul", 41.01f, 28.98f), new City("Kolkata", 22.57f, 88.36f),
            new City("Lagos", 6.52f, 3.38f), new City("Manila", 14.60f, 120.98f), new City("Rio de Janeiro", -22.91f, -43.17f),
            new City("Guangzhou", 23.13f, 113.26f), new City("Los Angeles", 34.05f, -118.24f), new City("Moscow", 55.76f, 37.62f),
            new City("Kinshasa", -4.44f, 15.27f), new City("Paris", 48.86f, 2.35f), new City("Jakarta", -6.21f, 106.85f),
            new City("Lima", -12.05f, -77.04f), new City("Bangkok", 13.76f, 100.50f), new City("Seoul", 37.57f, 126.98f),
            new City("London", 51.51f, -0.13f), new City("Tehran", 35.69f, 51.39f), new City("Chicago", 41.88f, -87.63f),
            new City("Bogotá", 4.71f, -74.07f), new City("Johannesburg", -26.20f, 28.05f), new City("Baghdad", 33.31f, 44.36f),
            new City("Riyadh", 24.71f, 46.68f), new City("Singapore", 1.35f, 103.82f), new City("Sydney", -33.87f, 151.21f),
            new City("Melbourne", -37.81f, 144.96f), new City("Toronto", 43.65f, -79.38f), new City("Madrid", 40.42f, -3.70f),
            new City("Berlin", 52.52f, 13.40f), new City("Rome", 41.90f, 12.50f), new City("Nairobi", -1.29f, 36.82f),
            new City("Santiago", -33.45f, -70.67f), new City("Hong Kong", 22.32f, 114.17f), new City("Brussels", 50.85f, 4.35f),
            new City("Amsterdam", 52.37f, 4.90f), new City("Dubai", 25.20f, 55.27f), new City("San Francisco", 37.77f, -122.42f),
            new City("Vancouver", 49.28f, -123.12f), new City("Casablanca", 33.57f, -7.59f), new City("Addis Ababa", 9.03f, 38.74f),
            new City("Karachi", 24.86f, 67.01f), new City("Ho Chi Minh City", 10.82f, 106.63f), new City("Kuala Lumpur", 3.14f, 101.69f),
            new City("Stockholm", 59.33f, 18.07f), new City("Athens", 37.98f, 23.73f), new City("Cape Town", -33.92f, 18.42f),
            new City("Miami", 25.76f, -80.19f), new City("Havana", 23.11f, -82.37f), new City("Auckland", -36.85f, 174.76f),
        };
    }
}
