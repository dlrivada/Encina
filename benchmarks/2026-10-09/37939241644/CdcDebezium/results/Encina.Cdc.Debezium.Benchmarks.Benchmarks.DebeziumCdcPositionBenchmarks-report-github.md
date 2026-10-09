```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  6.322 ns | 0.7813 ns | 0.0428 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 37.297 ns | 2.4691 ns | 0.1353 ns |  5.90 |    0.04 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 31.461 ns | 2.7575 ns | 0.1511 ns |  4.98 |    0.04 | 0.0091 |     152 B |        6.33 |
