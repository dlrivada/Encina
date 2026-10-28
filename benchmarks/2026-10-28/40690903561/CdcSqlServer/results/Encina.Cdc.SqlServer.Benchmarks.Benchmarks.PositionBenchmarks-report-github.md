```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions |  1.275 ns |  0.0240 ns | 0.0013 ns |  0.11 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 11.519 ns | 15.9691 ns | 0.8753 ns |  1.00 |    0.09 | 0.0014 |      24 B |        1.00 |
| FromBytes        |  7.438 ns |  2.1587 ns | 0.1183 ns |  0.65 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          |  6.409 ns |  0.1239 ns | 0.0068 ns |  0.56 |    0.04 | 0.0019 |      32 B |        1.33 |
