```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   160.30 ns |    56.289 ns |   3.085 ns |  1.02 |    0.02 |    2 | 0.0038 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    95.92 ns |     6.769 ns |   0.371 ns |  0.61 |    0.00 |    1 | 0.0013 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,989.46 ns |   160.079 ns |   8.774 ns | 19.08 |    0.05 |    3 | 0.0267 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,423.35 ns | 2,230.545 ns | 122.264 ns | 21.85 |    0.68 |    3 | 0.0076 | 0.0038 |     848 B |        2.41 |
| GetExistingKey         | 12          |   156.65 ns |     1.024 ns |   0.056 ns |  1.00 |    0.00 |    2 | 0.0041 |      - |     352 B |        1.00 |
