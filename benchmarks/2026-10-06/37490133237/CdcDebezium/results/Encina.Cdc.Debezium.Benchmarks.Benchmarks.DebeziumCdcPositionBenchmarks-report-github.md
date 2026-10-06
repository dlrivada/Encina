```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  8.836 ns |  5.163 ns | 0.2830 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 49.729 ns | 12.231 ns | 0.6704 ns |  5.63 |    0.17 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 40.575 ns |  2.982 ns | 0.1634 ns |  4.60 |    0.13 | 0.0091 |     152 B |        6.33 |
