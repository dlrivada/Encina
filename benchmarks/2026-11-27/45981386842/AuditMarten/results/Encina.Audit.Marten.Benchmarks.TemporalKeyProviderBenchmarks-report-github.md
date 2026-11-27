```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |    89.18 ns |    12.803 ns |  0.702 ns |  1.08 |    0.04 |    2 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    54.89 ns |     7.390 ns |  0.405 ns |  0.66 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 1,588.95 ns |   146.072 ns |  8.007 ns | 19.23 |    0.65 |    3 | 0.1469 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 2,817.58 ns | 1,240.646 ns | 68.004 ns | 34.10 |    1.34 |    4 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |    82.72 ns |    59.419 ns |  3.257 ns |  1.00 |    0.05 |    2 | 0.0210 |      - |     352 B |        1.00 |
