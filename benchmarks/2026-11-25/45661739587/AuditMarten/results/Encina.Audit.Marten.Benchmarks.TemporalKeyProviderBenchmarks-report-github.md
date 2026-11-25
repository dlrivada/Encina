```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   154.35 ns |    29.250 ns |   1.603 ns |  0.98 |    0.01 |    2 | 0.0038 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    94.29 ns |     2.086 ns |   0.114 ns |  0.60 |    0.00 |    1 | 0.0013 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,934.30 ns |    29.791 ns |   1.633 ns | 18.72 |    0.02 |    3 | 0.0267 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 2,975.06 ns | 2,966.377 ns | 162.597 ns | 18.98 |    0.90 |    3 | 0.0076 | 0.0038 |     848 B |        2.41 |
| GetExistingKey         | 12          |   156.72 ns |     3.617 ns |   0.198 ns |  1.00 |    0.00 |    2 | 0.0041 |      - |     352 B |        1.00 |
