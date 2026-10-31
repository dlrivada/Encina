```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 109.415 ns |  4.0838 ns | 0.2238 ns | 16.59 |    0.04 | 0.0011 |      96 B |        4.00 |
| CreatePosition   |   6.597 ns |  0.2631 ns | 0.0144 ns |  1.00 |    0.00 | 0.0003 |      24 B |        1.00 |
| FromBytes        | 469.168 ns | 17.0611 ns | 0.9352 ns | 71.12 |    0.18 | 0.0119 |    1024 B |       42.67 |
| ToBytes          | 456.258 ns | 22.8985 ns | 1.2551 ns | 69.16 |    0.21 | 0.0119 |    1008 B |       42.00 |
