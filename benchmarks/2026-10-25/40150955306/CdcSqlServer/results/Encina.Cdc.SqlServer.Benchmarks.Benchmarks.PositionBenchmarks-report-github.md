```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.6095 ns | 0.0689 ns | 0.1011 ns | 0.6735 ns |  0.17 |    0.03 |      - |         - |        0.00 |
| CreatePosition   | 3.6698 ns | 0.0852 ns | 0.1222 ns | 3.7127 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.9808 ns | 0.0871 ns | 0.1192 ns | 3.9817 ns |  1.09 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.7257 ns | 0.0697 ns | 0.1021 ns | 3.7230 ns |  1.02 |    0.04 | 0.0019 |      32 B |        1.33 |
