```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  4.598 ns |  0.8212 ns | 0.0450 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 26.584 ns | 14.8719 ns | 0.8152 ns |  5.78 |    0.16 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 20.710 ns |  2.3126 ns | 0.1268 ns |  4.50 |    0.04 | 0.0091 |     152 B |        6.33 |
