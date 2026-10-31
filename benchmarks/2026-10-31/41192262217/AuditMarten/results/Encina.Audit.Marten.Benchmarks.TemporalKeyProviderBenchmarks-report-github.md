```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|-------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   129.25 ns |     5.383 ns |  0.295 ns |  1.19 |    0.01 |    1 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    90.93 ns |    13.480 ns |  0.739 ns |  0.84 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,180.78 ns |   229.872 ns | 12.600 ns | 20.14 |    0.15 |    2 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,756.76 ns | 1,119.947 ns | 61.388 ns | 34.69 |    0.53 |    3 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   108.30 ns |    12.415 ns |  0.681 ns |  1.00 |    0.01 |    1 | 0.0210 |      - |     352 B |        1.00 |
