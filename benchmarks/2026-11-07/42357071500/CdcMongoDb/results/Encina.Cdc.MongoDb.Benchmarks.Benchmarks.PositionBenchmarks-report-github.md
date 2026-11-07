```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 112.508 ns |  49.940 ns |  2.7374 ns | 13.07 |    0.32 | 0.0011 |      96 B |        4.00 |
| CreatePosition   |   8.611 ns |   2.187 ns |  0.1199 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 455.778 ns | 339.481 ns | 18.6081 ns | 52.94 |    1.98 | 0.0119 |    1024 B |       42.67 |
| ToBytes          | 427.524 ns |  87.472 ns |  4.7946 ns | 49.65 |    0.77 | 0.0119 |    1008 B |       42.00 |
