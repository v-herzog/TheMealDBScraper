using Newtonsoft.Json.Linq;

namespace TheMealDBScraper;

static class Program
{
    static async Task Main()
    {
        Console.WriteLine("Scrapping TheMealDB for recipes...");
        
        var apiUrl = "https://www.themealdb.com/api/json/v1/1/search.php?f=";
        var letters = Enumerable.Range('a', 'z' - 'a' + 1).Select(c => Convert.ToChar(c));

        var fileContent = new JArray();

        foreach (var letter in letters)
        {
            Console.WriteLine($"Getting recipes with the letter {letter}.");
            
            var response = await GetApiResponse(apiUrl + letter);
            var recipes = ParseResponse(response);

            foreach (var recipe in recipes)
            {
                fileContent.Add(recipe);
            }
        }
        
        SaveRecipesToJson(fileContent);
    }

    static async Task<string> GetApiResponse(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Console.WriteLine($"Failed to retrieve data. Status Code: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }
    }

    static JArray ParseResponse(string response)
    {
        try
        {
            var jsonResponse = JObject.Parse(response);
            var meals = jsonResponse["meals"];

            if (meals != null && meals.HasValues)
            {
                var recipes = new JArray();
                foreach (var meal in meals)
                {
                    recipes.Add(JToken.FromObject(new
                    {
                        Id = meal["idMeal"],
                        Name = meal["strMeal"],
                        Category = meal["strCategory"],
                        Area = meal["strArea"],
                        Instructions = new JArray(meal["strInstructions"]
                            .Value<string>()
                            .Split("\r\n")
                            .Where(x => !string.IsNullOrWhiteSpace(x))),
                        Ingredients = ParseIngredients(meal),
                        ImageSrc = meal["strMealThumb"],
                        YoutubeLink = meal["strYoutube"],
                    }));
                }

                return recipes;
            }
            else
            {
                Console.WriteLine("No recipes found.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while parsing the response: {ex.Message}");
        }

        return new JArray();
    }
    
    static JArray ParseIngredients(JToken meal)
    {
        var ingredients = new JArray();
        
        for (var i = 1; i <= 20; i++)
        {
            var ingredient = meal[$"strIngredient{i}"];
            
            if (ingredient != null && !string.IsNullOrWhiteSpace(ingredient.Value<string>()))
            {
                ingredients.Add(JToken.FromObject(new
                {
                    Name = ingredient,
                    Measure = meal[$"strMeasure{i}"],
                }));
            }
        }

        return ingredients;
    }
    
    static void SaveRecipesToJson(JArray fileContent)
    {
        var fileName = "/home/deck/Documents/TheMealDBScraper/TheMealDBScraper/recipes.json";
        
        File.WriteAllText(fileName, fileContent.ToString());
        Console.WriteLine($"{fileContent.Count} recipes saved to file.");
    }
}