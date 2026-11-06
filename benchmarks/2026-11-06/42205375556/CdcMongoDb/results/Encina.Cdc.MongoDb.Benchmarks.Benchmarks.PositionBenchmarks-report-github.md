```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 133.614 ns |   4.415 ns |  0.2420 ns | 16.70 |    0.11 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   8.003 ns |   1.094 ns |  0.0600 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 619.404 ns |   4.714 ns |  0.2584 ns | 77.40 |    0.50 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 583.364 ns | 233.080 ns | 12.7759 ns | 72.89 |    1.46 | 0.0601 |    1008 B |       42.00 |
