```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |    85.21 ns |  41.250 ns |  2.261 ns |  1.06 |    0.03 |    2 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    53.47 ns |   2.618 ns |  0.144 ns |  0.67 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 1,546.65 ns | 176.363 ns |  9.667 ns | 19.32 |    0.45 |    3 | 0.1469 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,000.21 ns | 255.737 ns | 14.018 ns | 37.48 |    0.86 |    4 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |    80.09 ns |  38.593 ns |  2.115 ns |  1.00 |    0.03 |    2 | 0.0210 |      - |     352 B |        1.00 |
