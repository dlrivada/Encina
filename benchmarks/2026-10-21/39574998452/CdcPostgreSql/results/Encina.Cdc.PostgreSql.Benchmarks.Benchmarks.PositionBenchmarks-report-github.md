```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| ComparePositions | 0.4426 ns | 0.0210 ns | 0.0011 ns |  0.14 |      - |         - |        0.00 |
| CreatePosition   | 3.2146 ns | 0.3035 ns | 0.0166 ns |  1.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.4352 ns | 0.1113 ns | 0.0061 ns |  1.07 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.3244 ns | 0.0691 ns | 0.0038 ns |  1.03 | 0.0019 |      32 B |        1.33 |
