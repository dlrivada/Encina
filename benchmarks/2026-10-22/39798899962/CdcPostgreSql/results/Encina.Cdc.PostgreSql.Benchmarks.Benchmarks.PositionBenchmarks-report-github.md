```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.023 ns |  0.0218 ns | 0.0012 ns |  0.18 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 5.783 ns |  0.2525 ns | 0.0138 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 5.325 ns |  3.0164 ns | 0.1653 ns |  0.92 |    0.02 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.864 ns | 10.5345 ns | 0.5774 ns |  1.01 |    0.09 | 0.0019 |      32 B |        1.33 |
