```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error       | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   150.29 ns |    70.22 ns |   3.849 ns |  0.99 |    0.02 |    2 | 0.0038 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    76.58 ns |    32.87 ns |   1.802 ns |  0.51 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,172.40 ns | 1,744.49 ns |  95.621 ns | 14.36 |    0.56 |    3 | 0.0267 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 2,576.61 ns | 5,198.31 ns | 284.937 ns | 17.03 |    1.64 |    3 | 0.0076 | 0.0038 |     848 B |        2.41 |
| GetExistingKey         | 12          |   151.31 ns |    21.74 ns |   1.192 ns |  1.00 |    0.01 |    2 | 0.0041 |      - |     352 B |        1.00 |
