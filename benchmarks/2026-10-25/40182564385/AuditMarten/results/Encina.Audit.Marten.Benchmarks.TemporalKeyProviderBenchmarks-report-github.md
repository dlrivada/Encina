```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |    96.96 ns |    24.83 ns |  1.361 ns |  1.16 |    0.02 |    2 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    62.32 ns |    69.00 ns |  3.782 ns |  0.75 |    0.04 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 1,702.58 ns | 1,226.07 ns | 67.205 ns | 20.36 |    0.74 |    3 | 0.1469 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,060.60 ns |   661.95 ns | 36.284 ns | 36.61 |    0.60 |    4 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |    83.62 ns |    22.40 ns |  1.228 ns |  1.00 |    0.02 |    2 | 0.0210 |      - |     352 B |        1.00 |
