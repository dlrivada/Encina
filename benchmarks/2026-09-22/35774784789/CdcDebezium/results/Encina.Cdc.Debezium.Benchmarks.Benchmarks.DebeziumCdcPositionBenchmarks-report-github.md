```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  8.177 ns |  0.9555 ns | 0.0524 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 52.933 ns | 21.1671 ns | 1.1602 ns |  6.47 |    0.13 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 48.091 ns |  5.4437 ns | 0.2984 ns |  5.88 |    0.05 | 0.0091 |     152 B |        6.33 |
