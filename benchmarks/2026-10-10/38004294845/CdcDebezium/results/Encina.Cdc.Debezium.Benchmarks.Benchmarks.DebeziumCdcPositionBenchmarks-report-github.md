```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  9.463 ns |  0.8445 ns | 0.0463 ns |  1.00 |    0.01 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 70.403 ns | 37.9663 ns | 2.0811 ns |  7.44 |    0.19 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 53.009 ns |  8.4576 ns | 0.4636 ns |  5.60 |    0.05 | 0.0018 |     152 B |        6.33 |
