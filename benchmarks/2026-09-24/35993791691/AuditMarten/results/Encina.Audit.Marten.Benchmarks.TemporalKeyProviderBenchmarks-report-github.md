```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   162.0 ns |     9.98 ns |   0.55 ns |  1.19 |    0.01 |    2 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   109.9 ns |     8.99 ns |   0.49 ns |  0.80 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,719.0 ns |    18.51 ns |   1.01 ns | 19.90 |    0.17 |    3 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 4,117.3 ns | 1,935.66 ns | 106.10 ns | 30.13 |    0.72 |    4 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   136.7 ns |    24.10 ns |   1.32 ns |  1.00 |    0.01 |    2 | 0.0210 |      - |     352 B |        1.00 |
