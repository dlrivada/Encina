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
| CreatePosition |  8.894 ns | 1.8527 ns | 0.1016 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 66.412 ns | 3.3576 ns | 0.1840 ns |  7.47 |    0.08 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 50.749 ns | 0.6447 ns | 0.0353 ns |  5.71 |    0.06 | 0.0018 |     152 B |        6.33 |
