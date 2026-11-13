```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |    95.47 ns |    72.125 ns |  3.953 ns |  1.20 |    0.05 |    2 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    56.49 ns |     5.207 ns |  0.285 ns |  0.71 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 1,626.87 ns |   987.129 ns | 54.108 ns | 20.42 |    0.64 |    3 | 0.1469 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 2,979.58 ns | 1,703.162 ns | 93.356 ns | 37.41 |    1.12 |    4 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |    79.67 ns |    20.847 ns |  1.143 ns |  1.00 |    0.02 |    2 | 0.0210 |      - |     352 B |        1.00 |
