```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |    85.07 ns |    53.889 ns |  2.954 ns |  1.18 |    0.04 |    2 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    56.66 ns |    10.244 ns |  0.561 ns |  0.79 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 1,492.24 ns |   285.563 ns | 15.653 ns | 20.69 |    0.20 |    3 | 0.1469 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 2,702.71 ns | 1,555.515 ns | 85.263 ns | 37.47 |    1.03 |    4 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |    72.13 ns |     5.824 ns |  0.319 ns |  1.00 |    0.01 |    2 | 0.0210 |      - |     352 B |        1.00 |
