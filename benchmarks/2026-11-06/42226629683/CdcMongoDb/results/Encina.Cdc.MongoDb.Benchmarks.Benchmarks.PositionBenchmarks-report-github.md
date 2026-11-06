```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.11GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 134.421 ns |   3.708 ns | 0.2033 ns | 17.81 |    0.19 | 0.0057 |      96 B |        4.00 |
| CreatePosition   |   7.548 ns |   1.669 ns | 0.0915 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 622.179 ns |  79.572 ns | 4.3616 ns | 82.44 |    1.00 | 0.0610 |    1024 B |       42.67 |
| ToBytes          | 585.875 ns | 178.317 ns | 9.7742 ns | 77.63 |    1.39 | 0.0601 |    1008 B |       42.00 |
