```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   113.44 ns |    61.796 ns |   3.387 ns |  1.07 |    0.03 |    2 | 0.0038 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    82.04 ns |     7.953 ns |   0.436 ns |  0.78 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,233.13 ns | 3,686.334 ns | 202.060 ns | 21.12 |    1.66 |    3 | 0.0267 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 2,674.83 ns | 1,677.412 ns |  91.945 ns | 25.29 |    0.77 |    4 | 0.0076 | 0.0038 |     848 B |        2.41 |
| GetExistingKey         | 12          |   105.76 ns |    12.881 ns |   0.706 ns |  1.00 |    0.01 |    2 | 0.0042 |      - |     352 B |        1.00 |
