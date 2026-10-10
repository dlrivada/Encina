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
| CreatePosition |  7.412 ns |  2.205 ns | 0.1209 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 50.008 ns | 14.877 ns | 0.8154 ns |  6.75 |    0.13 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 45.730 ns |  3.379 ns | 0.1852 ns |  6.17 |    0.09 | 0.0091 |     152 B |        6.33 |
