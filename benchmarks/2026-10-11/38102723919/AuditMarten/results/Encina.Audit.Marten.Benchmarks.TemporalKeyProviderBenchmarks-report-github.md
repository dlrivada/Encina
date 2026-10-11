```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | PeriodCount | Mean       | Error    | StdDev    | Median     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|---------:|----------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   161.5 ns |  0.64 ns |   0.94 ns |   161.7 ns |  1.19 |    0.01 |    3 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   109.8 ns |  0.25 ns |   0.36 ns |   109.8 ns |  0.81 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,746.5 ns | 33.73 ns |  49.45 ns | 2,719.4 ns | 20.23 |    0.39 |    4 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 4,310.8 ns | 93.03 ns | 130.41 ns | 4,277.4 ns | 31.75 |    0.98 |    5 | 0.0458 | 0.0381 |     848 B |        2.41 |
| GetExistingKey         | 12          |   135.8 ns |  0.78 ns |   1.12 ns |   136.0 ns |  1.00 |    0.01 |    2 | 0.0210 |      - |     352 B |        1.00 |
