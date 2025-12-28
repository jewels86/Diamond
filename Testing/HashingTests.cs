using Diamond;
using static Diamond.Hashing;

namespace Testing;

public class HashingTests
{
    public static void TestSHA256()
    {
        var results = AcceleratedSHA256Hex(["abc", "", "hello world"]);
        
        Console.WriteLine($"abc -> {SHA256Hex("abc")}, accelerated {results[0]}");
        Console.WriteLine($"'' -> {SHA256Hex("")}, accelerated {results[1]}");
        Console.WriteLine($"hello world -> {SHA256Hex("hello world")}, accelerated {results[2]}");
    }
}