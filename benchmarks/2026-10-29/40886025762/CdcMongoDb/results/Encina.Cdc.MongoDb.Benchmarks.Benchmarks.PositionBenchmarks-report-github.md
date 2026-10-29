```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 138.481 ns |  13.338 ns |  0.7311 ns | 15.78 |    0.26 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.776 ns |   2.956 ns |  0.1620 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 654.795 ns | 248.711 ns | 13.6327 ns | 74.63 |    1.80 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 606.396 ns |  45.411 ns |  2.4891 ns | 69.11 |    1.13 | 0.0601 |    1008 B |       42.00 |
