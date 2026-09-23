```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.765 ns | 1.526 ns | 0.0837 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 51.335 ns | 8.600 ns | 0.4714 ns |  6.61 |    0.08 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 46.076 ns | 8.447 ns | 0.4630 ns |  5.93 |    0.08 | 0.0091 |     152 B |        6.33 |
