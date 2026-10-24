```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  9.046 ns | 1.3317 ns | 0.0730 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 67.547 ns | 6.6651 ns | 0.3653 ns |  7.47 |    0.06 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 48.549 ns | 0.5758 ns | 0.0316 ns |  5.37 |    0.04 | 0.0018 |     152 B |        6.33 |
