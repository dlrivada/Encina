```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   128.33 ns |  20.579 ns |  1.128 ns |  1.15 |    0.01 |    2 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    83.81 ns |   4.757 ns |  0.261 ns |  0.75 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,150.38 ns | 280.667 ns | 15.384 ns | 19.23 |    0.12 |    3 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,623.45 ns | 595.756 ns | 32.655 ns | 32.41 |    0.26 |    4 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   111.81 ns |   4.131 ns |  0.226 ns |  1.00 |    0.00 |    2 | 0.0210 |      - |     352 B |        1.00 |
