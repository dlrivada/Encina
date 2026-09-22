```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   120.23 ns |    42.800 ns |   2.346 ns |  1.09 |    0.03 |    2 | 0.0038 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    71.57 ns |     8.306 ns |   0.455 ns |  0.65 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,043.74 ns |   307.775 ns |  16.870 ns | 18.55 |    0.38 |    3 | 0.0267 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 2,540.91 ns | 2,338.075 ns | 128.158 ns | 23.06 |    1.10 |    4 | 0.0076 | 0.0038 |     848 B |        2.41 |
| GetExistingKey         | 12          |   110.22 ns |    44.496 ns |   2.439 ns |  1.00 |    0.03 |    2 | 0.0042 |      - |     352 B |        1.00 |
