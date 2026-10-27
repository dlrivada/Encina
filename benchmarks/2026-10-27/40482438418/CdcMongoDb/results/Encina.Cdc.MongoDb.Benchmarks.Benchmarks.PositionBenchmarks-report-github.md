```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 136.94 ns |   1.517 ns | 0.083 ns | 13.52 |    1.09 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |  10.19 ns |  17.369 ns | 0.952 ns |  1.01 |    0.11 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 639.38 ns | 102.199 ns | 5.602 ns | 63.13 |    5.10 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 593.97 ns |  74.067 ns | 4.060 ns | 58.64 |    4.73 | 0.0601 |    1008 B |       42.00 |
