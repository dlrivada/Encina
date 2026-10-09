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
| GetOrCreateExistingKey | 12          |   110.05 ns |    15.530 ns |   0.851 ns |  1.08 |    0.01 |    1 | 0.0038 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    89.43 ns |     6.539 ns |   0.358 ns |  0.87 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,117.17 ns | 2,616.270 ns | 143.407 ns | 20.71 |    1.23 |    2 | 0.0267 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 2,486.68 ns | 4,645.228 ns | 254.621 ns | 24.32 |    2.17 |    3 | 0.0076 | 0.0038 |     848 B |        2.41 |
| GetExistingKey         | 12          |   102.26 ns |    22.410 ns |   1.228 ns |  1.00 |    0.01 |    1 | 0.0042 |      - |     352 B |        1.00 |
