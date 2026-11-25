```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   137.78 ns |    53.820 ns |   2.950 ns |  1.01 |    0.02 |    2 | 0.0038 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    98.59 ns |     3.380 ns |   0.185 ns |  0.72 |    0.00 |    1 | 0.0013 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,550.12 ns |   270.035 ns |  14.802 ns | 18.63 |    0.10 |    3 | 0.0267 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 2,684.94 ns | 2,641.505 ns | 144.790 ns | 19.62 |    0.92 |    3 | 0.0076 | 0.0038 |     848 B |        2.41 |
| GetExistingKey         | 12          |   136.86 ns |     6.607 ns |   0.362 ns |  1.00 |    0.00 |    2 | 0.0041 |      - |     352 B |        1.00 |
