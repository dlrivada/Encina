```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.46GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |    97.78 ns |  50.64 ns |  2.776 ns |  1.20 |    0.03 |    3 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    60.59 ns |  63.02 ns |  3.454 ns |  0.74 |    0.04 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 1,592.98 ns | 360.71 ns | 19.772 ns | 19.58 |    0.27 |    4 | 0.1469 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,094.09 ns |  77.55 ns |  4.251 ns | 38.04 |    0.32 |    5 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |    81.35 ns |  14.26 ns |  0.781 ns |  1.00 |    0.01 |    2 | 0.0210 |      - |     352 B |        1.00 |
