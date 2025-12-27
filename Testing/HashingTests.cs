using Diamond;
using static Diamond.Hashing;

namespace Testing;

public class HashingTests
{
    public static void TestSHA256()
    {
        Console.WriteLine($"abc -> {SHA256Hex("abc")}");
        Console.WriteLine($"'' -> {SHA256Hex("")}");
        Console.WriteLine($"hello world -> {SHA256Hex("hello world")}");
    }
}