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
| CreatePosition |  8.253 ns | 0.9071 ns | 0.0497 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 48.239 ns | 7.4259 ns | 0.4070 ns |  5.85 |    0.05 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 40.198 ns | 5.7440 ns | 0.3148 ns |  4.87 |    0.04 | 0.0091 |     152 B |        6.33 |
