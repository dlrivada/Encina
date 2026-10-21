```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error       | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   151.51 ns |    80.69 ns |   4.423 ns |  0.97 |    0.03 |    2 | 0.0038 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    92.07 ns |    11.57 ns |   0.634 ns |  0.59 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,909.71 ns |   154.42 ns |   8.464 ns | 18.68 |    0.22 |    3 | 0.0267 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,337.72 ns | 2,824.61 ns | 154.826 ns | 21.43 |    0.90 |    3 | 0.0076 | 0.0038 |     848 B |        2.41 |
| GetExistingKey         | 12          |   155.78 ns |    37.43 ns |   2.052 ns |  1.00 |    0.02 |    2 | 0.0041 |      - |     352 B |        1.00 |
