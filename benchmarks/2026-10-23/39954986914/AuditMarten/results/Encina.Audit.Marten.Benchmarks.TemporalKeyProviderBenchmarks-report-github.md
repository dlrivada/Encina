```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |    83.79 ns |    37.195 ns |  2.039 ns |  1.14 |    0.02 |    2 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    54.02 ns |     1.652 ns |  0.091 ns |  0.74 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 1,487.18 ns |   128.837 ns |  7.062 ns | 20.24 |    0.09 |    3 | 0.1469 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 2,792.68 ns | 1,265.348 ns | 69.358 ns | 38.00 |    0.82 |    4 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |    73.49 ns |     3.321 ns |  0.182 ns |  1.00 |    0.00 |    2 | 0.0210 |      - |     352 B |        1.00 |
