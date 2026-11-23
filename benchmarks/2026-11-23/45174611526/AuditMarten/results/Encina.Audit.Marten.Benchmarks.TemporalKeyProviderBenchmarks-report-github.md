```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error       | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |    99.15 ns |    30.91 ns |   1.695 ns |  1.18 |    0.05 |    2 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    58.70 ns |    12.77 ns |   0.700 ns |  0.70 |    0.03 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 1,726.66 ns |   233.52 ns |  12.800 ns | 20.48 |    0.86 |    3 | 0.1469 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,137.13 ns | 1,905.77 ns | 104.462 ns | 37.21 |    1.88 |    4 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |    84.45 ns |    75.60 ns |   4.144 ns |  1.00 |    0.06 |    2 | 0.0210 |      - |     352 B |        1.00 |
