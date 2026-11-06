```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |    94.28 ns |    64.163 ns |   3.517 ns |  1.19 |    0.05 |    2 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    59.78 ns |     2.889 ns |   0.158 ns |  0.76 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 1,606.01 ns |   238.824 ns |  13.091 ns | 20.32 |    0.64 |    3 | 0.1469 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,046.79 ns | 3,116.306 ns | 170.815 ns | 38.56 |    2.22 |    4 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |    79.09 ns |    52.256 ns |   2.864 ns |  1.00 |    0.04 |    2 | 0.0210 |      - |     352 B |        1.00 |
