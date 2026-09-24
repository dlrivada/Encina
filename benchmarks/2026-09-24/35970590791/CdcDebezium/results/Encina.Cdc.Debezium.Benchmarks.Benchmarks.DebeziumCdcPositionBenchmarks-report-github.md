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
| CreatePosition |  7.899 ns |  4.203 ns | 0.2304 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 51.285 ns | 15.471 ns | 0.8480 ns |  6.50 |    0.19 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 45.312 ns | 18.991 ns | 1.0410 ns |  5.74 |    0.18 | 0.0091 |     152 B |        6.33 |
