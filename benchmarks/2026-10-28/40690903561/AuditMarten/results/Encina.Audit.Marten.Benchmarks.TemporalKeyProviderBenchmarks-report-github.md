```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   136.27 ns |    24.607 ns |  1.349 ns |  1.23 |    0.01 |    3 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    82.85 ns |     5.717 ns |  0.313 ns |  0.75 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,170.31 ns |   479.728 ns | 26.295 ns | 19.60 |    0.25 |    4 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,820.33 ns | 1,729.746 ns | 94.813 ns | 34.50 |    0.78 |    5 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   110.75 ns |    17.430 ns |  0.955 ns |  1.00 |    0.01 |    2 | 0.0210 |      - |     352 B |        1.00 |
