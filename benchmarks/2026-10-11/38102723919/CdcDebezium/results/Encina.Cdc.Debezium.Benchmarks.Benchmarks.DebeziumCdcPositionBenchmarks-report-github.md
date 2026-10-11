```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.429 ns | 0.0476 ns | 0.0683 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 49.773 ns | 0.3084 ns | 0.4616 ns |  6.70 |    0.09 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 44.980 ns | 0.1881 ns | 0.2697 ns |  6.06 |    0.07 | 0.0091 |     152 B |        6.33 |
