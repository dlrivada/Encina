```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.783 ns |  1.046 ns | 0.0573 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 51.908 ns | 20.963 ns | 1.1491 ns |  6.67 |    0.13 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 47.816 ns |  2.915 ns | 0.1598 ns |  6.14 |    0.04 | 0.0091 |     152 B |        6.33 |
