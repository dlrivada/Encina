```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 0.4367 ns | 0.0353 ns | 0.0019 ns |  0.14 |      - |         - |        0.00 |
| CreatePosition   | 3.2214 ns | 0.2497 ns | 0.0137 ns |  1.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.4453 ns | 0.2036 ns | 0.0112 ns |  1.07 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.1267 ns | 0.2372 ns | 0.0130 ns |  0.97 | 0.0019 |      32 B |        1.33 |
