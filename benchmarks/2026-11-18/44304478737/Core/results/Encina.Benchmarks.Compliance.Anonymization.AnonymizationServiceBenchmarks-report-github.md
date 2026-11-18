```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean         | Error       | StdDev      | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|------------:|------------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8,450.2 ns |  3,471.4 ns |   190.28 ns |  1.00 |    0.03 |    5 |  0.2289 | 0.0916 |    4098 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8,502.9 ns |  8,136.6 ns |   445.99 ns |  1.01 |    0.05 |    5 |  0.2289 | 0.0916 |    4154 B |        1.01 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,283.3 ns |    463.3 ns |    25.40 ns |  0.27 |    0.01 |    3 |  0.0305 |      - |     512 B |        0.12 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,215.7 ns |    101.7 ns |     5.57 ns |  0.14 |    0.00 |    2 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   3,939.0 ns |  4,352.6 ns |   238.58 ns |  0.47 |    0.03 |    4 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     839.1 ns |    197.2 ns |    10.81 ns |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 193,194.5 ns | 43,722.8 ns | 2,396.59 ns | 22.87 |    0.51 |    6 | 17.3340 | 0.4883 |  291296 B |       71.08 |
