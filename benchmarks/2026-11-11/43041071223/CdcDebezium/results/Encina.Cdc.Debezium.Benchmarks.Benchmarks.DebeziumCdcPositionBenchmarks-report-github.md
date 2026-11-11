```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.372 ns |  1.716 ns | 0.0941 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 50.982 ns | 19.229 ns | 1.0540 ns |  6.92 |    0.15 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 40.780 ns |  1.962 ns | 0.1076 ns |  5.53 |    0.06 | 0.0091 |     152 B |        6.33 |
