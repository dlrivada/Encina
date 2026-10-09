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
| CreatePosition |  7.957 ns |  6.246 ns | 0.3424 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 53.992 ns | 35.670 ns | 1.9552 ns |  6.79 |    0.33 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 45.831 ns |  8.301 ns | 0.4550 ns |  5.77 |    0.22 | 0.0091 |     152 B |        6.33 |
