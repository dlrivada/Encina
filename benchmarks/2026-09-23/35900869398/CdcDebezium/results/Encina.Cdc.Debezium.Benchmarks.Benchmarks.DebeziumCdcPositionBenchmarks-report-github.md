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
| CreatePosition |  8.601 ns |  3.940 ns | 0.2160 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 50.386 ns | 16.616 ns | 0.9108 ns |  5.86 |    0.16 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 40.345 ns |  1.276 ns | 0.0699 ns |  4.69 |    0.10 | 0.0091 |     152 B |        6.33 |
