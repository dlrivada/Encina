```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   131.83 ns |  11.076 ns | 0.607 ns |  1.19 |    0.03 |    2 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    82.91 ns |   5.617 ns | 0.308 ns |  0.75 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,093.22 ns | 124.992 ns | 6.851 ns | 18.85 |    0.43 |    3 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,578.24 ns |  67.576 ns | 3.704 ns | 32.23 |    0.73 |    4 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   111.08 ns |  53.425 ns | 2.928 ns |  1.00 |    0.03 |    2 | 0.0210 |      - |     352 B |        1.00 |
