```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   134.87 ns |   110.603 ns |   6.063 ns |  1.11 |    0.04 |    2 | 0.0038 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    79.11 ns |    49.706 ns |   2.725 ns |  0.65 |    0.02 |    1 | 0.0013 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,165.41 ns | 1,151.122 ns |  63.097 ns | 17.86 |    0.45 |    3 | 0.0267 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 2,661.92 ns | 5,462.863 ns | 299.438 ns | 21.96 |    2.14 |    4 | 0.0076 | 0.0038 |     848 B |        2.41 |
| GetExistingKey         | 12          |   121.24 ns |     7.834 ns |   0.429 ns |  1.00 |    0.00 |    2 | 0.0041 |      - |     352 B |        1.00 |
