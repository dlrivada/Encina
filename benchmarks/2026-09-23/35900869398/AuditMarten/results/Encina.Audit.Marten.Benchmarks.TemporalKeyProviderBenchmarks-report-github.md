```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   151.89 ns |    68.846 ns |  3.774 ns |  1.25 |    0.03 |    3 | 0.0038 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    78.39 ns |     7.115 ns |  0.390 ns |  0.64 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,076.84 ns |   152.041 ns |  8.334 ns | 17.03 |    0.17 |    4 | 0.0267 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 2,678.12 ns | 1,015.548 ns | 55.666 ns | 21.96 |    0.44 |    5 | 0.0076 | 0.0038 |     848 B |        2.41 |
| GetExistingKey         | 12          |   121.96 ns |    23.866 ns |  1.308 ns |  1.00 |    0.01 |    2 | 0.0042 |      - |     352 B |        1.00 |
