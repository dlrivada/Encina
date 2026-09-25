```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  8.311 ns |  3.591 ns | 0.1968 ns |  1.00 |    0.03 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 50.607 ns | 10.822 ns | 0.5932 ns |  6.09 |    0.14 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 44.743 ns | 25.458 ns | 1.3954 ns |  5.39 |    0.18 | 0.0018 |     152 B |        6.33 |
