```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   135.66 ns |    91.213 ns |  5.000 ns |  1.23 |    0.04 |    3 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    83.55 ns |     7.412 ns |  0.406 ns |  0.76 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,124.78 ns |   269.496 ns | 14.772 ns | 19.34 |    0.20 |    4 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,576.27 ns | 1,626.966 ns | 89.180 ns | 32.56 |    0.75 |    5 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   109.86 ns |    18.800 ns |  1.030 ns |  1.00 |    0.01 |    2 | 0.0210 |      - |     352 B |        1.00 |
