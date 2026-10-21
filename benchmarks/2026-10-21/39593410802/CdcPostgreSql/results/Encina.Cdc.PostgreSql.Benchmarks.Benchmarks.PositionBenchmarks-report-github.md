```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 0.4692 ns | 0.0743 ns | 0.0041 ns |  0.14 |      - |         - |        0.00 |
| CreatePosition   | 3.3587 ns | 0.2396 ns | 0.0131 ns |  1.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.7189 ns | 0.3236 ns | 0.0177 ns |  1.11 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.5150 ns | 0.4170 ns | 0.0229 ns |  1.05 | 0.0019 |      32 B |        1.33 |
